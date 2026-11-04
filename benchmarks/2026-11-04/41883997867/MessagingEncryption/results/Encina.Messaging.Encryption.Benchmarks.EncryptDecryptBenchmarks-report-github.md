```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.88GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  4.262 μs | 0.0219 μs | 0.0145 μs |  1.16 |    0.01 | 0.0381 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 31.841 μs | 0.3779 μs | 0.2500 μs |  8.63 |    0.08 | 2.3193 | 0.4883 |  197225 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  3.271 μs | 0.0668 μs | 0.0442 μs |  0.89 |    0.01 | 0.0267 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 22.967 μs | 0.1856 μs | 0.1227 μs |  6.23 |    0.05 | 1.5564 |      - |  131440 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  2.805 μs | 0.0372 μs | 0.0246 μs |  0.76 |    0.01 | 0.0038 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  3.689 μs | 0.0371 μs | 0.0245 μs |  1.00 |    0.01 | 0.0076 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  6.641 μs | 0.1444 μs | 0.0955 μs |  1.80 |    0.03 | 0.0076 |      - |    1184 B |        1.61 |
|                 |            |                |             |           |           |           |       |         |        |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  4.230 μs | 0.1838 μs | 0.0101 μs |  1.14 |    0.00 | 0.0381 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 31.949 μs | 4.6409 μs | 0.2544 μs |  8.61 |    0.07 | 2.3193 | 0.4883 |  197225 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  3.240 μs | 0.7653 μs | 0.0419 μs |  0.87 |    0.01 | 0.0267 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 23.257 μs | 2.3938 μs | 0.1312 μs |  6.27 |    0.04 | 1.5564 |      - |  131440 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  2.962 μs | 0.2229 μs | 0.0122 μs |  0.80 |    0.00 | 0.0038 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           |  3.711 μs | 0.2789 μs | 0.0153 μs |  1.00 |    0.01 | 0.0076 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  6.710 μs | 0.3833 μs | 0.0210 μs |  1.81 |    0.01 | 0.0076 |      - |    1184 B |        1.61 |
