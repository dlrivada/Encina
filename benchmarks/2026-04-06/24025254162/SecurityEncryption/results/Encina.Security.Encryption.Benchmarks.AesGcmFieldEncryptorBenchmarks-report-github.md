```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 3.11GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|-------:|----------:|------------:|
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 4.623 μs | 0.0166 μs | 0.0110 μs |  1.00 | 0.0229 |      - |     448 B |        1.00 |
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 4.814 μs | 0.0103 μs | 0.0068 μs |  1.04 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 6.876 μs | 0.0318 μs | 0.0210 μs |  1.49 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 3.474 μs | 0.0115 μs | 0.0069 μs |  0.75 | 0.0191 |      - |     320 B |        0.71 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 7.911 μs | 0.0157 μs | 0.0082 μs |  1.71 | 0.0305 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 4.901 μs | 0.0099 μs | 0.0059 μs |  1.06 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 4.758 μs | 0.0107 μs | 0.0064 μs |  1.03 | 0.0687 |      - |    1168 B |        2.61 |
|                         |            |                |             |          |           |           |       |        |        |           |             |
| EncryptString_Short     | ShortRun   | 3              | 1           | 4.623 μs | 0.2210 μs | 0.0121 μs |  1.00 | 0.0229 |      - |     448 B |        1.00 |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 5.188 μs | 0.2118 μs | 0.0116 μs |  1.12 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 7.215 μs | 0.2257 μs | 0.0124 μs |  1.56 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 3.472 μs | 0.1196 μs | 0.0066 μs |  0.75 | 0.0191 |      - |     320 B |        0.71 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 8.486 μs | 0.4774 μs | 0.0262 μs |  1.84 | 0.0305 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 4.927 μs | 0.3845 μs | 0.0211 μs |  1.07 | 0.0229 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 4.749 μs | 0.2011 μs | 0.0110 μs |  1.03 | 0.0687 |      - |    1168 B |        2.61 |
