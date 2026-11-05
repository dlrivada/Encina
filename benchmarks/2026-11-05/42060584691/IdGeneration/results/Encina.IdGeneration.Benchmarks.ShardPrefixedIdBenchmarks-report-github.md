```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                         | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |-----------:|---------:|--------:|------:|-------:|----------:|------------:|
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 1,057.9 ns |  2.45 ns | 1.46 ns |  1.12 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 1,032.2 ns |  6.60 ns | 3.93 ns |  1.10 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     |   940.8 ns |  3.45 ns | 2.28 ns |  1.00 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     |   915.3 ns |  1.31 ns | 0.69 ns |  0.97 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     |   988.1 ns |  2.90 ns | 1.72 ns |  1.05 | 0.0057 |     120 B |        1.00 |
|                                |            |                |             |            |          |         |       |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 1,049.9 ns | 57.82 ns | 3.17 ns |  1.08 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 1,025.4 ns | 37.98 ns | 2.08 ns |  1.06 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           |   970.4 ns |  9.55 ns | 0.52 ns |  1.00 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           |   905.6 ns | 63.67 ns | 3.49 ns |  0.93 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           |   996.9 ns | 35.97 ns | 1.97 ns |  1.03 | 0.0057 |     120 B |        1.00 |
