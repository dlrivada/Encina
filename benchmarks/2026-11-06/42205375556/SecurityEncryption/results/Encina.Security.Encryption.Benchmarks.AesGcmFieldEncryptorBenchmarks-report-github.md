```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.95GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 4.050 μs | 0.0121 μs | 0.0072 μs |  1.06 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 6.838 μs | 0.0140 μs | 0.0083 μs |  1.79 | 0.0381 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 3.916 μs | 0.0167 μs | 0.0099 μs |  1.03 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 3.971 μs | 0.0025 μs | 0.0015 μs |  1.04 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 2.846 μs | 0.0086 μs | 0.0057 μs |  0.75 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 6.483 μs | 0.0703 μs | 0.0465 μs |  1.70 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 3.810 μs | 0.0174 μs | 0.0115 μs |  1.00 | 0.0229 |      - |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |        |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 4.068 μs | 0.2270 μs | 0.0124 μs |  1.05 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 6.852 μs | 0.5489 μs | 0.0301 μs |  1.77 | 0.0381 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 3.817 μs | 0.1710 μs | 0.0094 μs |  0.99 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 3.980 μs | 0.3759 μs | 0.0206 μs |  1.03 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 2.890 μs | 0.1634 μs | 0.0090 μs |  0.75 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 6.184 μs | 0.5356 μs | 0.0294 μs |  1.60 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 3.870 μs | 0.2846 μs | 0.0156 μs |  1.00 | 0.0229 |      - |     448 B |        1.00 |
