```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 4.897 μs | 0.0223 μs | 0.0148 μs |  1.04 | 0.0458 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 8.018 μs | 0.0382 μs | 0.0253 μs |  1.70 | 0.0153 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 4.738 μs | 0.0162 μs | 0.0097 μs |  1.01 | 0.0153 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 4.921 μs | 0.0188 μs | 0.0125 μs |  1.05 | 0.0458 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 3.301 μs | 0.0235 μs | 0.0156 μs |  0.70 | 0.0114 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 7.123 μs | 0.0406 μs | 0.0268 μs |  1.51 | 0.5035 | 0.0153 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 4.708 μs | 0.0099 μs | 0.0059 μs |  1.00 | 0.0153 |      - |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |        |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 4.939 μs | 1.3412 μs | 0.0735 μs |  1.06 | 0.0458 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 7.896 μs | 0.5541 μs | 0.0304 μs |  1.69 | 0.0153 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 4.630 μs | 0.5065 μs | 0.0278 μs |  0.99 | 0.0153 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 4.728 μs | 0.2034 μs | 0.0111 μs |  1.01 | 0.0458 |      - |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 3.224 μs | 0.0841 μs | 0.0046 μs |  0.69 | 0.0114 |      - |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 6.981 μs | 0.1925 μs | 0.0106 μs |  1.49 | 0.5035 | 0.0153 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 4.675 μs | 0.1532 μs | 0.0084 μs |  1.00 | 0.0153 |      - |     448 B |        1.00 |
