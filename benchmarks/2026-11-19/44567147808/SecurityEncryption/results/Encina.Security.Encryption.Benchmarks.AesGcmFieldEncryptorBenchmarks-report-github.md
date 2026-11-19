```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.07GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 2.469 μs | 0.0143 μs | 0.0075 μs |  1.02 |    0.01 | 0.0114 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 4.146 μs | 0.0243 μs | 0.0127 μs |  1.71 |    0.01 | 0.0076 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 2.218 μs | 0.0082 μs | 0.0054 μs |  0.91 |    0.01 | 0.0038 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 2.473 μs | 0.0266 μs | 0.0176 μs |  1.02 |    0.01 | 0.0114 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 1.822 μs | 0.0060 μs | 0.0040 μs |  0.75 |    0.00 | 0.0038 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 3.990 μs | 0.1991 μs | 0.1317 μs |  1.64 |    0.05 | 0.1450 |      - |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 2.428 μs | 0.0197 μs | 0.0130 μs |  1.00 |    0.01 | 0.0038 |      - |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |         |        |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 2.561 μs | 0.2492 μs | 0.0137 μs |  1.07 |    0.01 | 0.0114 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 4.324 μs | 1.5003 μs | 0.0822 μs |  1.80 |    0.03 | 0.0076 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 2.230 μs | 0.0620 μs | 0.0034 μs |  0.93 |    0.00 | 0.0038 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 2.406 μs | 0.4564 μs | 0.0250 μs |  1.00 |    0.01 | 0.0114 |      - |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 1.808 μs | 0.0381 μs | 0.0021 μs |  0.75 |    0.00 | 0.0038 |      - |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 3.672 μs | 1.4440 μs | 0.0791 μs |  1.53 |    0.03 | 0.1488 | 0.0038 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 2.398 μs | 0.1931 μs | 0.0106 μs |  1.00 |    0.01 | 0.0038 |      - |     448 B |        1.00 |
