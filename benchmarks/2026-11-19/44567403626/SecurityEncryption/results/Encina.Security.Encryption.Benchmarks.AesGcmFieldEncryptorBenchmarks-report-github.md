```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 2.523 μs | 0.0204 μs | 0.0135 μs |  1.07 | 0.0114 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 4.271 μs | 0.0175 μs | 0.0104 μs |  1.80 | 0.0076 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 2.167 μs | 0.0074 μs | 0.0049 μs |  0.91 | 0.0038 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 2.437 μs | 0.0207 μs | 0.0137 μs |  1.03 | 0.0114 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 1.807 μs | 0.0077 μs | 0.0046 μs |  0.76 | 0.0038 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 3.573 μs | 0.0246 μs | 0.0146 μs |  1.51 | 0.1488 | 0.0038 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 2.369 μs | 0.0134 μs | 0.0080 μs |  1.00 | 0.0038 |      - |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |        |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 2.466 μs | 0.0684 μs | 0.0037 μs |  1.03 | 0.0114 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 4.300 μs | 0.2128 μs | 0.0117 μs |  1.80 | 0.0076 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 2.155 μs | 0.0915 μs | 0.0050 μs |  0.90 | 0.0038 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 2.459 μs | 0.4449 μs | 0.0244 μs |  1.03 | 0.0114 |      - |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 1.791 μs | 0.0846 μs | 0.0046 μs |  0.75 | 0.0038 |      - |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 3.575 μs | 0.5646 μs | 0.0309 μs |  1.50 | 0.1488 | 0.0038 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 2.388 μs | 0.2174 μs | 0.0119 μs |  1.00 | 0.0038 |      - |     448 B |        1.00 |
