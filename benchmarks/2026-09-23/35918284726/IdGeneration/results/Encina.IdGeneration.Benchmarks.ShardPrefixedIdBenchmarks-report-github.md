```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error       | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|------------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 872.2 ns |    14.95 ns |  7.82 ns |  1.12 |    0.01 | 0.0086 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 870.5 ns |    32.88 ns | 21.75 ns |  1.12 |    0.03 | 0.0124 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 777.3 ns |    12.74 ns |  7.58 ns |  1.00 |    0.01 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 741.4 ns |    19.90 ns | 13.16 ns |  0.95 |    0.02 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 793.8 ns |    13.79 ns |  7.21 ns |  1.02 |    0.01 | 0.0067 |     120 B |        1.00 |
|                                |            |                |             |          |             |          |       |         |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 921.4 ns |    37.35 ns |  2.05 ns |  1.11 |    0.07 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 888.5 ns | 1,255.37 ns | 68.81 ns |  1.07 |    0.10 | 0.0124 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 835.0 ns | 1,132.92 ns | 62.10 ns |  1.00 |    0.09 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 732.5 ns |   125.91 ns |  6.90 ns |  0.88 |    0.06 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 796.7 ns |   537.21 ns | 29.45 ns |  0.96 |    0.07 | 0.0067 |     120 B |        1.00 |
