```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 2.420 μs | 0.0203 μs | 0.0121 μs |  1.02 |    0.01 | 0.0114 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 4.240 μs | 0.0606 μs | 0.0360 μs |  1.79 |    0.02 | 0.0076 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 2.232 μs | 0.0074 μs | 0.0049 μs |  0.94 |    0.00 | 0.0038 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 2.458 μs | 0.0214 μs | 0.0141 μs |  1.04 |    0.01 | 0.0114 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 1.887 μs | 0.0175 μs | 0.0116 μs |  0.80 |    0.01 | 0.0038 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 3.734 μs | 0.0584 μs | 0.0386 μs |  1.58 |    0.02 | 0.1488 | 0.0038 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 2.364 μs | 0.0136 μs | 0.0090 μs |  1.00 |    0.01 | 0.0038 |      - |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |         |        |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 2.435 μs | 0.2187 μs | 0.0120 μs |  1.02 |    0.00 | 0.0114 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 4.172 μs | 0.6662 μs | 0.0365 μs |  1.75 |    0.01 | 0.0076 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 2.214 μs | 0.0794 μs | 0.0044 μs |  0.93 |    0.00 | 0.0038 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 2.456 μs | 0.2004 μs | 0.0110 μs |  1.03 |    0.00 | 0.0114 |      - |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 1.808 μs | 0.3251 μs | 0.0178 μs |  0.76 |    0.01 | 0.0038 |      - |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 3.794 μs | 0.5917 μs | 0.0324 μs |  1.59 |    0.01 | 0.1488 | 0.0038 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 2.381 μs | 0.0988 μs | 0.0054 μs |  1.00 |    0.00 | 0.0038 |      - |     448 B |        1.00 |
