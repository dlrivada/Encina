```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 5.007 μs | 0.0179 μs | 0.0106 μs |  0.99 |    0.00 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 7.792 μs | 0.0280 μs | 0.0185 μs |  1.55 |    0.00 | 0.0305 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 4.777 μs | 0.0728 μs | 0.0481 μs |  0.95 |    0.01 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 5.216 μs | 0.0097 μs | 0.0057 μs |  1.04 |    0.00 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 3.054 μs | 0.0066 μs | 0.0039 μs |  0.61 |    0.00 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 7.885 μs | 0.0984 μs | 0.0585 μs |  1.56 |    0.01 | 0.7477 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 5.039 μs | 0.0103 μs | 0.0061 μs |  1.00 |    0.00 | 0.0229 |      - |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |         |        |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 5.439 μs | 0.2928 μs | 0.0160 μs |  1.15 |    0.00 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 7.937 μs | 2.2735 μs | 0.1246 μs |  1.68 |    0.02 | 0.0305 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 4.713 μs | 0.0636 μs | 0.0035 μs |  1.00 |    0.00 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 4.878 μs | 0.2116 μs | 0.0116 μs |  1.04 |    0.00 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 3.093 μs | 0.0350 μs | 0.0019 μs |  0.66 |    0.00 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 7.479 μs | 1.0611 μs | 0.0582 μs |  1.59 |    0.01 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 4.712 μs | 0.1853 μs | 0.0102 μs |  1.00 |    0.00 | 0.0229 |      - |     448 B |        1.00 |
