```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 1,115.6 ns |   3.53 ns |  2.33 ns |  0.97 |    0.01 |      - |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 1,291.7 ns |  11.64 ns |  6.93 ns |  1.13 |    0.02 | 0.0019 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 1,146.0 ns |  23.31 ns | 15.42 ns |  1.00 |    0.02 |      - |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 1,012.9 ns |   5.68 ns |  3.38 ns |  0.88 |    0.01 |      - |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 1,185.3 ns |  16.76 ns | 11.08 ns |  1.03 |    0.02 |      - |     120 B |        1.00 |
|                                |            |                |             |            |           |          |       |         |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 1,123.7 ns |  97.78 ns |  5.36 ns |  0.94 |    0.01 |      - |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 1,220.5 ns |  90.80 ns |  4.98 ns |  1.02 |    0.01 | 0.0019 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 1,198.8 ns | 197.65 ns | 10.83 ns |  1.00 |    0.01 |      - |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           |   995.4 ns |  90.45 ns |  4.96 ns |  0.83 |    0.01 |      - |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 1,186.1 ns | 284.64 ns | 15.60 ns |  0.99 |    0.01 |      - |     120 B |        1.00 |
