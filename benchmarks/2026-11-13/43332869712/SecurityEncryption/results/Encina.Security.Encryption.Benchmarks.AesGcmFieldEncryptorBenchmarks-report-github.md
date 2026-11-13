```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 4.299 μs | 0.0298 μs | 0.0197 μs |  1.07 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 7.310 μs | 0.1042 μs | 0.0689 μs |  1.82 |    0.02 | 0.0381 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 4.281 μs | 0.0177 μs | 0.0105 μs |  1.06 |    0.01 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 4.518 μs | 0.0210 μs | 0.0110 μs |  1.12 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 2.751 μs | 0.0091 μs | 0.0054 μs |  0.68 |    0.01 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 6.997 μs | 0.1168 μs | 0.0611 μs |  1.74 |    0.02 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 4.028 μs | 0.0489 μs | 0.0323 μs |  1.00 |    0.01 | 0.0229 |      - |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |         |        |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 4.525 μs | 0.9455 μs | 0.0518 μs |  1.14 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 7.307 μs | 1.8979 μs | 0.1040 μs |  1.83 |    0.02 | 0.0381 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 4.028 μs | 0.2360 μs | 0.0129 μs |  1.01 |    0.01 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 4.190 μs | 0.5170 μs | 0.0283 μs |  1.05 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 2.651 μs | 0.2186 μs | 0.0120 μs |  0.67 |    0.00 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 6.939 μs | 2.1511 μs | 0.1179 μs |  1.74 |    0.03 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 3.983 μs | 0.3650 μs | 0.0200 μs |  1.00 |    0.01 | 0.0229 |      - |     448 B |        1.00 |
