```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 2.946 μs | 0.0942 μs | 0.0623 μs |  1.08 |    0.03 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 4.585 μs | 0.0885 μs | 0.0586 μs |  1.68 |    0.03 | 0.0381 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 2.774 μs | 0.0611 μs | 0.0364 μs |  1.02 |    0.02 | 0.0267 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 2.736 μs | 0.0370 μs | 0.0245 μs |  1.01 |    0.02 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 1.834 μs | 0.0340 μs | 0.0202 μs |  0.67 |    0.01 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 3.914 μs | 0.1494 μs | 0.0988 μs |  1.44 |    0.04 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 2.722 μs | 0.0657 μs | 0.0391 μs |  1.00 |    0.02 | 0.0267 |      - |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |         |        |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 2.806 μs | 0.4608 μs | 0.0253 μs |  1.05 |    0.02 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 4.411 μs | 0.2529 μs | 0.0139 μs |  1.64 |    0.03 | 0.0381 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 2.776 μs | 0.4150 μs | 0.0227 μs |  1.03 |    0.02 | 0.0267 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 2.849 μs | 0.4697 μs | 0.0257 μs |  1.06 |    0.02 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 1.747 μs | 0.5688 μs | 0.0312 μs |  0.65 |    0.01 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 3.763 μs | 1.5381 μs | 0.0843 μs |  1.40 |    0.03 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 2.685 μs | 0.8781 μs | 0.0481 μs |  1.00 |    0.02 | 0.0267 |      - |     448 B |        1.00 |
