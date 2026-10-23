```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                  | Job        | IterationCount | LaunchCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |---------:|-----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 2.955 μs |  0.0266 μs | 0.0139 μs |  1.05 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 4.637 μs |  0.0929 μs | 0.0486 μs |  1.64 |    0.02 | 0.0381 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 2.747 μs |  0.0406 μs | 0.0212 μs |  0.97 |    0.01 | 0.0267 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 2.887 μs |  0.1635 μs | 0.1082 μs |  1.02 |    0.04 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 1.827 μs |  0.0327 μs | 0.0195 μs |  0.65 |    0.01 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 3.918 μs |  0.0603 μs | 0.0359 μs |  1.39 |    0.02 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 2.823 μs |  0.0382 μs | 0.0253 μs |  1.00 |    0.01 | 0.0267 |      - |     448 B |        1.00 |
|                         |            |                |             |          |            |           |       |         |        |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 2.932 μs |  0.3572 μs | 0.0196 μs |  1.01 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 4.643 μs |  1.0917 μs | 0.0598 μs |  1.60 |    0.03 | 0.0381 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 2.867 μs |  2.3151 μs | 0.1269 μs |  0.99 |    0.04 | 0.0267 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 2.959 μs |  0.4824 μs | 0.0264 μs |  1.02 |    0.02 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 1.918 μs |  1.1705 μs | 0.0642 μs |  0.66 |    0.02 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 4.276 μs | 14.4602 μs | 0.7926 μs |  1.48 |    0.24 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 2.896 μs |  0.7877 μs | 0.0432 μs |  1.00 |    0.02 | 0.0267 |      - |     448 B |        1.00 |
