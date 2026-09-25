```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 4.245 μs | 0.0976 μs | 0.0581 μs |  1.02 |    0.02 | 0.0076 |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 7.227 μs | 0.1111 μs | 0.0661 μs |  1.74 |    0.04 | 0.0076 |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 3.954 μs | 0.1192 μs | 0.0789 μs |  0.95 |    0.03 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 4.376 μs | 0.0925 μs | 0.0612 μs |  1.06 |    0.02 | 0.0076 |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 2.778 μs | 0.0420 μs | 0.0219 μs |  0.67 |    0.01 | 0.0038 |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 5.771 μs | 0.2235 μs | 0.1330 μs |  1.39 |    0.04 | 0.1450 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 4.148 μs | 0.1451 μs | 0.0864 μs |  1.00 |    0.03 |      - |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |         |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 4.123 μs | 0.1458 μs | 0.0080 μs |  1.04 |    0.00 | 0.0076 |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 6.855 μs | 0.2943 μs | 0.0161 μs |  1.73 |    0.01 | 0.0076 |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 3.928 μs | 0.2517 μs | 0.0138 μs |  0.99 |    0.00 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 4.093 μs | 1.0714 μs | 0.0587 μs |  1.03 |    0.01 | 0.0076 |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 2.850 μs | 1.3775 μs | 0.0755 μs |  0.72 |    0.02 | 0.0038 |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 5.824 μs | 1.4214 μs | 0.0779 μs |  1.47 |    0.02 | 0.1450 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 3.957 μs | 0.3327 μs | 0.0182 μs |  1.00 |    0.01 |      - |     448 B |        1.00 |
