```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 3.880 μs | 0.0591 μs | 0.0391 μs |  0.99 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 6.027 μs | 0.0118 μs | 0.0070 μs |  1.53 | 0.0381 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 3.617 μs | 0.0083 μs | 0.0044 μs |  0.92 | 0.0267 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 3.754 μs | 0.0346 μs | 0.0229 μs |  0.95 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 2.330 μs | 0.0028 μs | 0.0015 μs |  0.59 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 5.694 μs | 0.1088 μs | 0.0569 μs |  1.45 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 3.939 μs | 0.0088 μs | 0.0053 μs |  1.00 | 0.0229 |      - |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |        |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 3.827 μs | 0.9550 μs | 0.0523 μs |  1.05 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 6.517 μs | 0.5940 μs | 0.0326 μs |  1.78 | 0.0381 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 3.863 μs | 0.2461 μs | 0.0135 μs |  1.06 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 3.761 μs | 0.2743 μs | 0.0150 μs |  1.03 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 2.540 μs | 0.1681 μs | 0.0092 μs |  0.70 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 5.606 μs | 0.7664 μs | 0.0420 μs |  1.53 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 3.653 μs | 0.3211 μs | 0.0176 μs |  1.00 | 0.0267 |      - |     448 B |        1.00 |
