```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.84GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 5.336 μs | 0.0258 μs | 0.0154 μs |  1.11 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 7.768 μs | 0.0872 μs | 0.0577 μs |  1.62 |    0.01 | 0.0305 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 4.669 μs | 0.0103 μs | 0.0068 μs |  0.98 |    0.00 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 5.160 μs | 0.0072 μs | 0.0043 μs |  1.08 |    0.00 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 3.062 μs | 0.0082 μs | 0.0054 μs |  0.64 |    0.00 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 7.542 μs | 0.1609 μs | 0.0957 μs |  1.58 |    0.02 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 4.787 μs | 0.0315 μs | 0.0187 μs |  1.00 |    0.01 | 0.0229 |      - |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |         |        |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 5.051 μs | 0.5177 μs | 0.0284 μs |  1.06 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 7.780 μs | 1.0141 μs | 0.0556 μs |  1.64 |    0.01 | 0.0305 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 5.038 μs | 0.1168 μs | 0.0064 μs |  1.06 |    0.00 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 4.914 μs | 0.2195 μs | 0.0120 μs |  1.03 |    0.00 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 3.055 μs | 0.1680 μs | 0.0092 μs |  0.64 |    0.00 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 7.992 μs | 1.4297 μs | 0.0784 μs |  1.68 |    0.01 | 0.7477 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 4.758 μs | 0.1208 μs | 0.0066 μs |  1.00 |    0.00 | 0.0229 |      - |     448 B |        1.00 |
