```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.15GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |-----------:|----------:|--------:|------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 1,051.8 ns |   2.98 ns | 1.56 ns |  1.05 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 1,059.2 ns |   3.25 ns | 2.15 ns |  1.06 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     |   999.7 ns |   3.85 ns | 2.01 ns |  1.00 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     |   894.5 ns |   4.87 ns | 3.22 ns |  0.89 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     |   950.3 ns |   2.54 ns | 1.51 ns |  0.95 | 0.0057 |     120 B |        1.00 |
|                                |            |                |             |            |           |         |       |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 1,098.9 ns | 137.21 ns | 7.52 ns |  1.16 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 1,064.6 ns |  83.69 ns | 4.59 ns |  1.12 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           |   949.7 ns |  59.73 ns | 3.27 ns |  1.00 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           |   893.4 ns |  78.79 ns | 4.32 ns |  0.94 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           |   954.9 ns |  22.46 ns | 1.23 ns |  1.01 | 0.0057 |     120 B |        1.00 |
