```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 4.903 μs | 0.0596 μs | 0.0394 μs |  1.05 |    0.01 | 0.0076 |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 7.878 μs | 0.0594 μs | 0.0354 μs |  1.68 |    0.02 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 4.601 μs | 0.0321 μs | 0.0191 μs |  0.98 |    0.01 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 5.003 μs | 0.0205 μs | 0.0107 μs |  1.07 |    0.01 | 0.0076 |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 3.225 μs | 0.0323 μs | 0.0192 μs |  0.69 |    0.01 | 0.0038 |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 6.983 μs | 0.0391 μs | 0.0204 μs |  1.49 |    0.02 | 0.1450 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 4.677 μs | 0.0893 μs | 0.0591 μs |  1.00 |    0.02 |      - |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |         |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 4.978 μs | 1.3750 μs | 0.0754 μs |  1.07 |    0.02 | 0.0076 |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 8.001 μs | 1.1512 μs | 0.0631 μs |  1.73 |    0.01 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 4.446 μs | 1.2865 μs | 0.0705 μs |  0.96 |    0.01 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 4.850 μs | 0.8931 μs | 0.0490 μs |  1.05 |    0.01 | 0.0076 |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 3.311 μs | 0.0537 μs | 0.0029 μs |  0.71 |    0.00 | 0.0038 |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 6.964 μs | 0.5589 μs | 0.0306 μs |  1.50 |    0.01 | 0.1450 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 4.637 μs | 0.5227 μs | 0.0287 μs |  1.00 |    0.01 |      - |     448 B |        1.00 |
