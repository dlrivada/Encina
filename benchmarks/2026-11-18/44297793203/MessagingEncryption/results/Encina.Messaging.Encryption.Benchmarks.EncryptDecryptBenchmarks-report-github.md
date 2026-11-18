```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method          | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|--------:|-------:|----------:|------------:|
| Encrypt_Medium  | Job-YFEFPZ | 10             | Default     |  3.615 μs | 0.0420 μs | 0.0278 μs |  1.13 |    0.01 |  0.2174 |      - |    3688 B |        5.01 |
| Encrypt_Large   | Job-YFEFPZ | 10             | Default     | 15.560 μs | 0.1266 μs | 0.0753 μs |  4.88 |    0.03 | 11.7493 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | Job-YFEFPZ | 10             | Default     |  2.539 μs | 0.0269 μs | 0.0160 μs |  0.80 |    0.01 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | Job-YFEFPZ | 10             | Default     | 12.132 μs | 0.2407 μs | 0.1432 μs |  3.81 |    0.04 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | Job-YFEFPZ | 10             | Default     |  2.093 μs | 0.0093 μs | 0.0049 μs |  0.66 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | Job-YFEFPZ | 10             | Default     |  3.186 μs | 0.0164 μs | 0.0108 μs |  1.00 |    0.00 |  0.0420 |      - |     736 B |        1.00 |
| Roundtrip_Short | Job-YFEFPZ | 10             | Default     |  5.828 μs | 0.0361 μs | 0.0215 μs |  1.83 |    0.01 |  0.0687 |      - |    1184 B |        1.61 |
|                 |            |                |             |           |           |           |       |         |         |        |           |             |
| Encrypt_Medium  | ShortRun   | 3              | 1           |  3.858 μs | 0.5745 μs | 0.0315 μs |  1.21 |    0.01 |  0.2174 |      - |    3688 B |        5.01 |
| Encrypt_Large   | ShortRun   | 3              | 1           | 16.276 μs | 4.4001 μs | 0.2412 μs |  5.13 |    0.07 | 11.7493 |      - |  197227 B |      267.97 |
| Decrypt_Medium  | ShortRun   | 3              | 1           |  2.326 μs | 0.0609 μs | 0.0033 μs |  0.73 |    0.00 |  0.1411 |      - |    2416 B |        3.28 |
| Decrypt_Large   | ShortRun   | 3              | 1           | 11.951 μs | 0.2758 μs | 0.0151 μs |  3.76 |    0.02 |  7.8430 | 0.9766 |  131442 B |      178.59 |
| Decrypt_Short   | ShortRun   | 3              | 1           |  2.032 μs | 0.0613 μs | 0.0034 μs |  0.64 |    0.00 |  0.0267 |      - |     448 B |        0.61 |
| Encrypt_Short   | ShortRun   | 3              | 1           |  3.175 μs | 0.3962 μs | 0.0217 μs |  1.00 |    0.01 |  0.0420 |      - |     736 B |        1.00 |
| Roundtrip_Short | ShortRun   | 3              | 1           |  5.336 μs | 0.0840 μs | 0.0046 μs |  1.68 |    0.01 |  0.0687 |      - |    1184 B |        1.61 |
