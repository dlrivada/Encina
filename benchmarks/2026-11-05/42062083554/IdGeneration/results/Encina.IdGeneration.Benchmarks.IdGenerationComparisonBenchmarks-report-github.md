```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 242.3 ns |  0.06 ns | 0.04 ns |  1.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 655.2 ns |  3.08 ns | 2.04 ns |  2.70 |    2 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 657.3 ns |  5.18 ns | 2.71 ns |  2.71 |    2 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 870.3 ns |  4.66 ns | 2.77 ns |  3.59 |    3 | 0.0124 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 679.0 ns |  2.21 ns | 1.32 ns |  2.80 |    2 |      - |         - |          NA |
|                    |            |                |             |          |          |         |       |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           | 242.3 ns |  1.34 ns | 0.07 ns |  1.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           | 657.9 ns | 43.61 ns | 2.39 ns |  2.72 |    2 | 0.0019 |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           | 654.1 ns | 48.48 ns | 2.66 ns |  2.70 |    2 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 866.7 ns | 27.84 ns | 1.53 ns |  3.58 |    3 | 0.0124 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           | 678.7 ns | 36.86 ns | 2.02 ns |  2.80 |    2 |      - |         - |          NA |
