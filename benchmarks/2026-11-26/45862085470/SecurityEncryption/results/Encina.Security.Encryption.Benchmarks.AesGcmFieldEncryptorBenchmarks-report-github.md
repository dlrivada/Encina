```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.12GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 3.405 μs | 0.0341 μs | 0.0226 μs |  1.07 |    0.02 | 0.0114 |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 5.840 μs | 0.1287 μs | 0.0673 μs |  1.83 |    0.04 | 0.0076 |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 3.030 μs | 0.1007 μs | 0.0666 μs |  0.95 |    0.03 | 0.0038 |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 3.296 μs | 0.0697 μs | 0.0461 μs |  1.03 |    0.02 | 0.0114 |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 2.506 μs | 0.0522 μs | 0.0311 μs |  0.78 |    0.02 | 0.0038 |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 5.339 μs | 0.1019 μs | 0.0606 μs |  1.67 |    0.03 | 0.1450 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 3.196 μs | 0.0915 μs | 0.0605 μs |  1.00 |    0.03 | 0.0038 |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |         |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 3.418 μs | 0.6410 μs | 0.0351 μs |  1.06 |    0.01 | 0.0114 |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 6.115 μs | 1.7424 μs | 0.0955 μs |  1.89 |    0.03 | 0.0076 |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 3.026 μs | 0.2028 μs | 0.0111 μs |  0.93 |    0.01 | 0.0038 |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 3.337 μs | 0.5274 μs | 0.0289 μs |  1.03 |    0.01 | 0.0114 |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 2.564 μs | 1.6775 μs | 0.0920 μs |  0.79 |    0.03 | 0.0038 |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 5.333 μs | 0.8357 μs | 0.0458 μs |  1.65 |    0.02 | 0.1450 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 3.239 μs | 0.3928 μs | 0.0215 μs |  1.00 |    0.01 | 0.0038 |     448 B |        1.00 |
