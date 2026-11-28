```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.09GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 3.666 μs | 0.0327 μs | 0.0195 μs |  1.04 |    0.01 | 0.0114 |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 6.350 μs | 0.1069 μs | 0.0707 μs |  1.81 |    0.02 | 0.0076 |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 3.272 μs | 0.0095 μs | 0.0056 μs |  0.93 |    0.00 | 0.0038 |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 3.585 μs | 0.0404 μs | 0.0211 μs |  1.02 |    0.01 | 0.0114 |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 2.686 μs | 0.0153 μs | 0.0101 μs |  0.76 |    0.00 | 0.0038 |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 5.731 μs | 0.0597 μs | 0.0312 μs |  1.63 |    0.01 | 0.1450 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 3.517 μs | 0.0315 μs | 0.0187 μs |  1.00 |    0.01 | 0.0038 |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |         |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 3.647 μs | 0.3342 μs | 0.0183 μs |  1.06 |    0.01 | 0.0114 |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 6.271 μs | 1.3317 μs | 0.0730 μs |  1.82 |    0.03 | 0.0076 |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 3.276 μs | 0.0985 μs | 0.0054 μs |  0.95 |    0.01 | 0.0038 |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 3.558 μs | 0.0332 μs | 0.0018 μs |  1.03 |    0.01 | 0.0114 |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 2.773 μs | 1.1244 μs | 0.0616 μs |  0.80 |    0.02 | 0.0038 |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 5.712 μs | 0.4888 μs | 0.0268 μs |  1.66 |    0.02 | 0.1450 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 3.445 μs | 0.7495 μs | 0.0411 μs |  1.00 |    0.01 | 0.0038 |     448 B |        1.00 |
