```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.58GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 3.805 μs | 0.0210 μs | 0.0110 μs |  1.04 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 6.017 μs | 0.0160 μs | 0.0106 μs |  1.64 |    0.01 | 0.0381 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 3.670 μs | 0.0096 μs | 0.0063 μs |  1.00 |    0.01 | 0.0267 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 3.708 μs | 0.0115 μs | 0.0076 μs |  1.01 |    0.01 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 2.535 μs | 0.0042 μs | 0.0028 μs |  0.69 |    0.01 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 5.533 μs | 0.1915 μs | 0.1267 μs |  1.51 |    0.03 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 3.664 μs | 0.0419 μs | 0.0277 μs |  1.00 |    0.01 | 0.0267 |      - |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |         |        |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 3.770 μs | 0.0768 μs | 0.0042 μs |  0.98 |    0.00 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 5.934 μs | 0.6388 μs | 0.0350 μs |  1.54 |    0.01 | 0.0381 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 3.920 μs | 1.4548 μs | 0.0797 μs |  1.01 |    0.02 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 3.672 μs | 0.1922 μs | 0.0105 μs |  0.95 |    0.00 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 2.373 μs | 0.4402 μs | 0.0241 μs |  0.61 |    0.01 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 5.260 μs | 0.3632 μs | 0.0199 μs |  1.36 |    0.00 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 3.865 μs | 0.0941 μs | 0.0052 μs |  1.00 |    0.00 | 0.0229 |      - |     448 B |        1.00 |
