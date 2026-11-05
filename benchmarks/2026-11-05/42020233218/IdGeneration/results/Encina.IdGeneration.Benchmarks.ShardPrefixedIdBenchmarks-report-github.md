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
| Generate_TimestampRandomFormat | Job-YFEFPZ | 10             | Default     | 1,095.6 ns |  4.34 ns | 2.87 ns |  1.16 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | Job-YFEFPZ | 10             | Default     | 1,063.2 ns |  2.56 ns | 1.52 ns |  1.12 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | Job-YFEFPZ | 10             | Default     |   947.5 ns |  5.22 ns | 3.45 ns |  1.00 | 0.0057 |     120 B |        1.00 |
| Generate_UuidV7Format          | Job-YFEFPZ | 10             | Default     |   898.9 ns |  3.56 ns | 2.12 ns |  0.95 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | Job-YFEFPZ | 10             | Default     |   954.0 ns |  4.91 ns | 3.25 ns |  1.01 | 0.0057 |     120 B |        1.00 |
|                                |            |                |             |            |          |         |       |        |           |             |
| Generate_TimestampRandomFormat | ShortRun   | 3              | 1           | 1,051.7 ns | 44.07 ns | 2.42 ns |  1.12 | 0.0076 |     151 B |        1.26 |
| Generate_ToString              | ShortRun   | 3              | 1           | 1,022.2 ns | 53.11 ns | 2.91 ns |  1.09 | 0.0114 |     216 B |        1.80 |
| Generate_UlidFormat            | ShortRun   | 3              | 1           |   940.1 ns | 25.83 ns | 1.42 ns |  1.00 | 0.0067 |     120 B |        1.00 |
| Generate_UuidV7Format          | ShortRun   | 3              | 1           |   933.9 ns | 64.82 ns | 3.55 ns |  0.99 | 0.0057 |      96 B |        0.80 |
| ExtractShardId_Ulid            | ShortRun   | 3              | 1           |   956.8 ns | 33.12 ns | 1.82 ns |  1.02 | 0.0057 |     120 B |        1.00 |
