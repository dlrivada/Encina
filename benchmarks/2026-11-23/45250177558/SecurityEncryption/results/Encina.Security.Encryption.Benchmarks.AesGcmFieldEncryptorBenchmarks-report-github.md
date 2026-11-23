```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.76GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 2.441 μs | 0.0276 μs | 0.0164 μs |  1.03 | 0.0114 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 4.216 μs | 0.0142 μs | 0.0094 μs |  1.78 | 0.0076 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 2.213 μs | 0.0089 μs | 0.0059 μs |  0.94 | 0.0038 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 2.428 μs | 0.0403 μs | 0.0266 μs |  1.03 | 0.0114 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 1.805 μs | 0.0059 μs | 0.0035 μs |  0.76 | 0.0038 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 3.749 μs | 0.0399 μs | 0.0209 μs |  1.59 | 0.1488 | 0.0038 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 2.364 μs | 0.0227 μs | 0.0150 μs |  1.00 | 0.0038 |      - |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |        |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 2.486 μs | 0.4522 μs | 0.0248 μs |  1.03 | 0.0114 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 4.259 μs | 0.4456 μs | 0.0244 μs |  1.76 | 0.0076 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 2.218 μs | 0.0735 μs | 0.0040 μs |  0.92 | 0.0038 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 2.406 μs | 0.1787 μs | 0.0098 μs |  1.00 | 0.0114 |      - |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 1.814 μs | 0.2342 μs | 0.0128 μs |  0.75 | 0.0038 |      - |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 3.709 μs | 0.2859 μs | 0.0157 μs |  1.54 | 0.1488 | 0.0038 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 2.415 μs | 0.1344 μs | 0.0074 μs |  1.00 | 0.0038 |      - |     448 B |        1.00 |
