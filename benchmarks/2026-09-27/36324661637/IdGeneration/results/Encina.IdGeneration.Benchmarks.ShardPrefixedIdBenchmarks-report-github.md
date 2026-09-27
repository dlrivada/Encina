```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.43GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 859.1 ns |  17.84 ns | 10.62 ns |  1.10 |    0.02 | 0.0086 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 833.2 ns |  10.96 ns |  6.52 ns |  1.07 |    0.01 | 0.0124 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 778.6 ns |  13.98 ns |  8.32 ns |  1.00 |    0.01 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 736.8 ns |  16.38 ns | 10.83 ns |  0.95 |    0.02 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 774.8 ns |  10.91 ns |  6.49 ns |  1.00 |    0.01 | 0.0067 |     120 B |        1.00 |
|                                |            |                |             |          |           |          |       |         |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 878.4 ns | 405.00 ns | 22.20 ns |  1.09 |    0.03 | 0.0086 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 833.3 ns |  78.97 ns |  4.33 ns |  1.03 |    0.02 | 0.0124 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 805.7 ns | 345.07 ns | 18.91 ns |  1.00 |    0.03 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 727.7 ns | 103.96 ns |  5.70 ns |  0.90 |    0.02 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 784.4 ns |  81.18 ns |  4.45 ns |  0.97 |    0.02 | 0.0067 |     120 B |        1.00 |
