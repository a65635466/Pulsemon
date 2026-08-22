# PulseMon

PulseMon은 Windows 시스템 트레이에서 실행되는 가벼운 실시간 시스템 상태 모니터링 프로그램입니다. WPF 기반의 작은 상태창을 통해 CPU, RAM, GPU, 네트워크 상태를 약 1초 간격으로 확인할 수 있습니다.

## 주요 기능

- Windows 시스템 트레이 아이콘 표시
- 트레이 아이콘 좌클릭으로 상태창 표시 또는 숨김
- 우클릭 메뉴에서 상태창 열기, 설정 열기, 프로그램 종료
- CPU 사용률 표시
- RAM 사용량과 전체 용량 표시
- GPU 사용률 표시
- GPU 온도 표시 영역 제공
- 다운로드 및 업로드 속도 표시
- 약 1초 간격의 자동 갱신
- 단일 인스턴스 실행
- 종료 시 트레이 아이콘과 모니터링 리소스 정리

## 기술 스택

- C#
- .NET 8
- WPF
- Windows Forms `NotifyIcon`
- Windows 성능 카운터 및 시스템 API

## 프로젝트 구조

```text
PulseMon/
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml
├── MainWindow.xaml.cs
├── SettingsWindow.xaml
├── SettingsWindow.xaml.cs
├── AppTheme.cs
├── PulseMon.csproj
│
├── Assets/
│   └── PulseMon.ico
│
├── Models/
│   ├── DeviceInfo.cs
│   └── SystemStatus.cs
│
├── Monitoring/
│   ├── CpuMonitor.cs
│   ├── MemoryMonitor.cs
│   ├── NetworkMonitor.cs
│   ├── GpuMonitor.cs
│   ├── NativeMethods.cs
│   └── Snapshot/Record 계열 파일
│
├── Services/
│   ├── DeviceInfoService.cs
│   └── MonitoringService.cs
│
├── Tray/
│   ├── TrayManager.cs
│   ├── TrayIconRenderer.cs
│   └── SafeIconHandle.cs
│
└── UI/
```

## 실행 흐름

1. `App.xaml.cs`가 프로그램 시작 시 단일 인스턴스 Mutex를 생성합니다.
2. `MainWindow`를 만들고, `TrayManager`를 초기화해 시스템 트레이 아이콘을 표시합니다.
3. `MainWindow`는 `DispatcherTimer`를 사용해 약 1초마다 상태 갱신을 요청합니다.
4. `MonitoringService`가 CPU, RAM, GPU, 네트워크 모니터를 호출합니다.
5. 각 모니터가 읽은 값은 `SystemStatus` 모델 하나로 조합됩니다.
6. `MainWindow`가 `SystemStatus` 값을 UI 텍스트와 진행 막대에 반영합니다.
7. 창 닫기 버튼은 앱을 종료하지 않고 창만 숨깁니다.
8. 트레이 메뉴의 `Exit`을 선택하면 타이머, 트레이 아이콘, GPU 성능 카운터, Mutex를 정리하고 종료합니다.

## 데이터 모델 동작 방식

PulseMon의 화면 갱신은 `Models/SystemStatus.cs`의 `SystemStatus` 모델을 중심으로 동작합니다.

```csharp
public sealed class SystemStatus
{
    public double CpuUsagePercent { get; init; }
    public double MemoryUsedGb { get; init; }
    public double MemoryTotalGb { get; init; }
    public double? GpuUsagePercent { get; init; }
    public double? GpuTemperatureCelsius { get; init; }
    public double DownloadMbps { get; init; }
    public double UploadMbps { get; init; }
    public DateTime UpdatedAt { get; init; }
}
```

이 모델은 UI가 표시해야 하는 현재 시스템 상태를 한 번의 스냅샷으로 표현합니다. 단위가 필요한 값은 속성명에 단위를 포함해 UI와 모니터링 코드 사이의 의미가 흐려지지 않도록 했습니다.

- `CpuUsagePercent`: CPU 사용률을 0부터 100 사이의 퍼센트로 저장합니다.
- `MemoryUsedGb`: 현재 사용 중인 메모리를 GB 단위로 저장합니다.
- `MemoryTotalGb`: 전체 메모리를 GB 단위로 저장합니다.
- `GpuUsagePercent`: GPU 사용률을 퍼센트로 저장하며, 가져올 수 없으면 `null`입니다.
- `GpuTemperatureCelsius`: GPU 온도를 섭씨로 저장하며, 가져올 수 없으면 `null`입니다.
- `DownloadMbps`: 다운로드 속도를 Mbps 단위로 저장합니다.
- `UploadMbps`: 업로드 속도를 Mbps 단위로 저장합니다.
- `UpdatedAt`: 상태값이 만들어진 시간을 저장합니다.

GPU 값은 하드웨어와 드라이버 환경에 따라 읽지 못할 수 있으므로 nullable 값으로 설계되어 있습니다. UI는 GPU 사용률과 온도가 모두 없으면 `N/A`로 표시합니다.

## 모니터링 구성

### CPU

`CpuMonitor`는 Windows `GetSystemTimes` API로 idle, kernel, user 시간을 읽습니다. 이전 측정값과 현재 측정값의 차이를 비교해 CPU 사용률을 계산합니다. 첫 측정에는 이전 값이 없으므로 `0`을 반환하고 다음 갱신부터 실제 변화량 기반 값이 표시됩니다.

### RAM

`MemoryMonitor`는 Windows 메모리 상태 API를 사용해 전체 메모리와 사용 가능한 메모리를 읽고, 사용 중인 메모리를 GB 단위로 계산합니다.

### Network

`NetworkMonitor`는 활성화된 IPv4 네트워크 인터페이스의 송수신 바이트를 합산합니다. Loopback과 Tunnel 인터페이스는 제외합니다. 이전 샘플과 현재 샘플의 바이트 차이를 시간 차이로 나누어 Mbps 단위의 다운로드 및 업로드 속도를 계산합니다. 음수 또는 비정상 값은 `0`으로 보정합니다.

### GPU

`GpuMonitor`는 Windows `GPU Engine` 성능 카운터의 `% Utilization` 값을 사용해 GPU 사용률을 계산합니다. 성능 카운터가 없거나 읽기에 실패하면 GPU 상태를 사용할 수 없는 값으로 처리하고 앱 전체는 계속 동작합니다.

현재 구현에서 GPU 온도는 별도 센서 라이브러리를 추가하지 않았기 때문에 `null`로 유지됩니다. UI에서는 온도 값을 `N/A`로 표시합니다.

## 서비스 계층

`MonitoringService`는 UI와 하드웨어 수집 로직 사이의 중간 계층입니다.

- `CpuMonitor`, `MemoryMonitor`, `NetworkMonitor`, `GpuMonitor`를 조합합니다.
- `GetCurrentStatus()` 호출 시 최신 상태를 `SystemStatus`로 반환합니다.
- 특정 모니터에서 예외가 발생해도 앱 전체가 멈추지 않도록 기본값 또는 `Unavailable` 값을 반환합니다.
- 마지막 읽기 오류는 `LastReadError`에 보관합니다.
- 종료 시 GPU 성능 카운터 리소스를 정리합니다.

## UI와 트레이

`MainWindow`는 직접 하드웨어 값을 읽지 않고 `MonitoringService`만 호출합니다. `DispatcherTimer`를 사용하므로 UI 스레드에서 안전하게 화면을 갱신합니다.

`TrayManager`는 Windows Forms `NotifyIcon`을 사용해 시스템 트레이 아이콘을 관리합니다. 좌클릭은 상태창 표시/숨김을 토글하고, 우클릭 메뉴는 상태창 열기, 설정 열기, 종료 기능을 제공합니다. 트레이 아이콘은 `TrayIconRenderer`에서 생성한 프레임으로 간단한 실행 애니메이션을 표시합니다.

## 빌드 및 실행

```powershell
dotnet build
dotnet run
```

실행 후 시스템 트레이에 PulseMon 아이콘이 표시됩니다. 트레이 아이콘을 클릭하면 상태창을 열거나 숨길 수 있습니다.

## 현재 제한사항

- GPU 온도는 아직 실제 센서에서 수집하지 않습니다.
- GPU 사용률은 Windows 성능 카운터 지원 여부에 따라 `N/A`로 표시될 수 있습니다.
- 장기 히스토리, 웹 대시보드, 로그인, 데이터베이스 저장, 자동 업데이트, 설치 프로그램은 1차 버전 범위에 포함하지 않습니다.
