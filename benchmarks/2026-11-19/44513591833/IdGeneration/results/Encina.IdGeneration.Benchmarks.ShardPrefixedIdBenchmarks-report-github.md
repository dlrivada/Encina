```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 764.4 ns |   3.54 ns |  2.34 ns |  0.91 |    0.01 | 0.0010 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 895.6 ns |  14.80 ns |  8.80 ns |  1.07 |    0.01 | 0.0019 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     | 836.0 ns |   9.91 ns |  5.90 ns |  1.00 |    0.01 | 0.0010 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     | 667.7 ns |   5.32 ns |  3.52 ns |  0.80 |    0.01 | 0.0010 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     | 843.7 ns |   8.68 ns |  5.74 ns |  1.01 |    0.01 | 0.0010 |     120 B |        1.00 |
|                                |            |                |             |          |           |          |       |         |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 759.6 ns |  21.75 ns |  1.19 ns |  0.90 |    0.01 | 0.0010 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 916.2 ns | 417.37 ns | 22.88 ns |  1.08 |    0.02 | 0.0019 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           | 846.4 ns | 137.82 ns |  7.55 ns |  1.00 |    0.01 | 0.0010 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           | 676.9 ns |  52.79 ns |  2.89 ns |  0.80 |    0.01 | 0.0010 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           | 846.8 ns |  63.89 ns |  3.50 ns |  1.00 |    0.01 | 0.0010 |     120 B |        1.00 |
