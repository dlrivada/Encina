```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.34GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 3.168 μs | 0.0556 μs | 0.0368 μs |  1.09 |    0.01 | 0.0114 |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 5.371 μs | 0.1171 μs | 0.0775 μs |  1.85 |    0.03 | 0.0076 |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 2.750 μs | 0.0190 μs | 0.0113 μs |  0.94 |    0.01 | 0.0038 |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 3.038 μs | 0.0553 μs | 0.0366 μs |  1.04 |    0.01 | 0.0114 |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 2.332 μs | 0.0547 μs | 0.0362 μs |  0.80 |    0.01 | 0.0038 |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 4.574 μs | 0.0851 μs | 0.0506 μs |  1.57 |    0.02 | 0.1450 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 2.910 μs | 0.0240 μs | 0.0143 μs |  1.00 |    0.01 | 0.0038 |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |         |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 3.090 μs | 0.2840 μs | 0.0156 μs |  1.07 |    0.01 | 0.0114 |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 5.305 μs | 1.0997 μs | 0.0603 μs |  1.84 |    0.02 | 0.0076 |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 2.770 μs | 0.1281 μs | 0.0070 μs |  0.96 |    0.01 | 0.0038 |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 3.035 μs | 0.2693 μs | 0.0148 μs |  1.05 |    0.01 | 0.0114 |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 2.365 μs | 0.3829 μs | 0.0210 μs |  0.82 |    0.01 | 0.0038 |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 4.730 μs | 1.6527 μs | 0.0906 μs |  1.64 |    0.03 | 0.1450 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 2.890 μs | 0.3513 μs | 0.0193 μs |  1.00 |    0.01 | 0.0038 |     448 B |        1.00 |
