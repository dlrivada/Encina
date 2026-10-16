```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 4.922 μs | 0.0206 μs | 0.0123 μs |  1.04 | 0.0458 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 7.931 μs | 0.0215 μs | 0.0112 μs |  1.68 | 0.0153 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 4.750 μs | 0.0279 μs | 0.0184 μs |  1.00 | 0.0153 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 4.865 μs | 0.0189 μs | 0.0112 μs |  1.03 | 0.0458 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 3.342 μs | 0.0206 μs | 0.0122 μs |  0.71 | 0.0114 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 7.265 μs | 0.0212 μs | 0.0140 μs |  1.54 | 0.5035 | 0.0153 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 4.731 μs | 0.0112 μs | 0.0074 μs |  1.00 | 0.0153 |      - |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |        |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 4.855 μs | 0.1386 μs | 0.0076 μs |  1.04 | 0.0458 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 7.949 μs | 0.4903 μs | 0.0269 μs |  1.71 | 0.0153 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 4.607 μs | 0.2405 μs | 0.0132 μs |  0.99 | 0.0153 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 4.669 μs | 0.0421 μs | 0.0023 μs |  1.00 | 0.0458 |      - |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 3.207 μs | 0.0392 μs | 0.0021 μs |  0.69 | 0.0114 |      - |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 7.181 μs | 0.2848 μs | 0.0156 μs |  1.54 | 0.5035 | 0.0153 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 4.655 μs | 0.1455 μs | 0.0080 μs |  1.00 | 0.0153 |      - |     448 B |        1.00 |
