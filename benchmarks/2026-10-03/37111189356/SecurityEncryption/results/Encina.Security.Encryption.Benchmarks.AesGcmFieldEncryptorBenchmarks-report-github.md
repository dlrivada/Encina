```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                  | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|-------:|----------:|------------:|
| EncryptString_Medium    | Job-YFEFPZ | 10             | Default     | 3.785 μs | 0.0033 μs | 0.0017 μs |  1.06 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | Job-YFEFPZ | 10             | Default     | 5.915 μs | 0.0119 μs | 0.0079 μs |  1.65 | 0.0381 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | Job-YFEFPZ | 10             | Default     | 3.651 μs | 0.0046 μs | 0.0024 μs |  1.02 | 0.0267 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | Job-YFEFPZ | 10             | Default     | 3.774 μs | 0.0152 μs | 0.0101 μs |  1.05 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | Job-YFEFPZ | 10             | Default     | 2.311 μs | 0.0054 μs | 0.0032 μs |  0.64 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | Job-YFEFPZ | 10             | Default     | 5.404 μs | 0.0202 μs | 0.0120 μs |  1.51 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | Job-YFEFPZ | 10             | Default     | 3.587 μs | 0.0125 μs | 0.0074 μs |  1.00 | 0.0267 |      - |     448 B |        1.00 |
|                         |            |                |             |          |           |           |       |        |        |           |             |
| EncryptString_Medium    | ShortRun   | 3              | 1           | 3.795 μs | 0.7039 μs | 0.0386 μs |  1.05 | 0.0687 |      - |    1168 B |        2.61 |
| EncryptDecryptRoundtrip | ShortRun   | 3              | 1           | 6.015 μs | 0.2437 μs | 0.0134 μs |  1.67 | 0.0381 |      - |     664 B |        1.48 |
| EncryptBytes_Short      | ShortRun   | 3              | 1           | 3.635 μs | 0.0458 μs | 0.0025 μs |  1.01 | 0.0267 |      - |     448 B |        1.00 |
| EncryptBytes_Medium     | ShortRun   | 3              | 1           | 3.721 μs | 0.0099 μs | 0.0005 μs |  1.03 | 0.0687 |      - |    1168 B |        2.61 |
| DecryptString_Short     | ShortRun   | 3              | 1           | 2.292 μs | 0.1605 μs | 0.0088 μs |  0.64 | 0.0191 |      - |     320 B |        0.71 |
| EncryptString_Long      | ShortRun   | 3              | 1           | 5.271 μs | 0.6667 μs | 0.0365 μs |  1.46 | 0.7553 | 0.0305 |   12688 B |       28.32 |
| EncryptString_Short     | ShortRun   | 3              | 1           | 3.605 μs | 0.0876 μs | 0.0048 μs |  1.00 | 0.0267 |      - |     448 B |        1.00 |
