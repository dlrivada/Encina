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
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 4.798 μs | 0.0481 μs | 0.0286 μs |  1.01 |    0.01 | 0.0076 |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 8.041 μs | 0.0249 μs | 0.0130 μs |  1.70 |    0.01 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 4.844 μs | 0.0716 μs | 0.0426 μs |  1.02 |    0.01 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 4.914 μs | 0.0273 μs | 0.0163 μs |  1.04 |    0.01 | 0.0076 |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 3.228 μs | 0.0275 μs | 0.0164 μs |  0.68 |    0.01 | 0.0038 |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 7.025 μs | 0.0519 μs | 0.0343 μs |  1.48 |    0.01 | 0.1450 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 4.733 μs | 0.0552 μs | 0.0365 μs |  1.00 |    0.01 |      - |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |         |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 4.982 μs | 0.6458 μs | 0.0354 μs |  1.06 |    0.01 | 0.0076 |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 8.194 μs | 2.0143 μs | 0.1104 μs |  1.74 |    0.02 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 4.606 μs | 0.9041 μs | 0.0496 μs |  0.98 |    0.01 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 4.841 μs | 0.9360 μs | 0.0513 μs |  1.03 |    0.01 | 0.0076 |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 3.265 μs | 0.8470 μs | 0.0464 μs |  0.69 |    0.01 | 0.0038 |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 7.120 μs | 2.8050 μs | 0.1538 μs |  1.51 |    0.03 | 0.1450 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 4.720 μs | 0.8134 μs | 0.0446 μs |  1.00 |    0.01 |      - |     448 B |        1.00 |
