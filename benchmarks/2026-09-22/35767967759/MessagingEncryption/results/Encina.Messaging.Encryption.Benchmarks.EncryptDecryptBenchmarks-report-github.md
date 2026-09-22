```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  5.359 μs | 0.0108 μs | 0.0065 μs |  1.12 |    0.00 | 0.1450 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 25.647 μs | 0.3198 μs | 0.1903 μs |  5.37 |    0.04 | 7.8125 | 1.7090 |  197226 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  3.737 μs | 0.0074 μs | 0.0049 μs |  0.78 |    0.00 | 0.0954 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 18.644 μs | 0.7409 μs | 0.4901 μs |  3.90 |    0.10 | 5.2185 | 0.6409 |  131441 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  3.311 μs | 0.0072 μs | 0.0043 μs |  0.69 |    0.00 | 0.0153 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  4.777 μs | 0.0163 μs | 0.0108 μs |  1.00 |    0.00 | 0.0229 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  8.408 μs | 0.0362 μs | 0.0239 μs |  1.76 |    0.01 | 0.0458 |      - |    1184 B |        1.61 |
|                 |            |                |             |           |           |           |       |         |        |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  5.427 μs | 0.1144 μs | 0.0063 μs |  1.12 |    0.00 | 0.1450 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 24.998 μs | 9.2007 μs | 0.5043 μs |  5.15 |    0.09 | 7.8125 | 1.7090 |  197226 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  3.796 μs | 0.1179 μs | 0.0065 μs |  0.78 |    0.00 | 0.0954 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 17.872 μs | 5.2597 μs | 0.2883 μs |  3.68 |    0.05 | 5.2185 | 0.6409 |  131441 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  3.313 μs | 0.0792 μs | 0.0043 μs |  0.68 |    0.00 | 0.0153 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           |  4.857 μs | 0.2766 μs | 0.0152 μs |  1.00 |    0.00 | 0.0229 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  8.412 μs | 0.3866 μs | 0.0212 μs |  1.73 |    0.01 | 0.0458 |      - |    1184 B |        1.61 |
