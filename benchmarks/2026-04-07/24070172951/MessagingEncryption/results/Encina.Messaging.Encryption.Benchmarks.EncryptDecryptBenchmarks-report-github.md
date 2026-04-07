```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.41GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  4.885 μs | 0.0157 μs | 0.0094 μs |  1.00 |    0.00 | 0.0229 |      - |     736 B |        1.00 |
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  5.109 μs | 0.0295 μs | 0.0176 μs |  1.05 |    0.00 | 0.1450 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 22.580 μs | 0.0708 μs | 0.0468 μs |  4.62 |    0.01 | 7.8125 | 1.7090 |  197226 B |      267.97 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  3.319 μs | 0.0159 μs | 0.0105 μs |  0.68 |    0.00 | 0.0153 |      - |     448 B |        0.61 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  3.638 μs | 0.0214 μs | 0.0142 μs |  0.74 |    0.00 | 0.0954 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 16.063 μs | 0.1192 μs | 0.0789 μs |  3.29 |    0.02 | 5.2185 | 0.6409 |  131441 B |      178.59 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  8.235 μs | 0.0319 μs | 0.0211 μs |  1.69 |    0.01 | 0.0458 |      - |    1184 B |        1.61 |
|                 |            |                |             |           |           |           |       |         |        |        |           |             |
| Encrypt_Short   | ShortRun   | 3              | 1           |  4.852 μs | 0.1553 μs | 0.0085 μs |  1.00 |    0.00 | 0.0229 |      - |     736 B |        1.00 |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  5.269 μs | 0.2930 μs | 0.0161 μs |  1.09 |    0.00 | 0.1450 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 22.242 μs | 3.4661 μs | 0.1900 μs |  4.58 |    0.03 | 7.8125 | 1.7090 |  197226 B |      267.97 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  3.316 μs | 0.1498 μs | 0.0082 μs |  0.68 |    0.00 | 0.0153 |      - |     448 B |        0.61 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  3.706 μs | 0.2938 μs | 0.0161 μs |  0.76 |    0.00 | 0.0954 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 16.094 μs | 1.1172 μs | 0.0612 μs |  3.32 |    0.01 | 5.2185 | 0.6409 |  131441 B |      178.59 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  8.270 μs | 0.9130 μs | 0.0500 μs |  1.70 |    0.01 | 0.0458 |      - |    1184 B |        1.61 |
