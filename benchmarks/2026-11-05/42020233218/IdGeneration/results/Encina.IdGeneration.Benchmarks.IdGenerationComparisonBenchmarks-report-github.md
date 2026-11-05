```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 242.3 ns |  0.06 ns | 0.04 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 655.6 ns |  3.71 ns | 2.21 ns |  2.71 |    0.01 |    2 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 651.2 ns |  3.32 ns | 1.74 ns |  2.69 |    0.01 |    2 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 846.8 ns |  2.95 ns | 1.75 ns |  3.49 |    0.01 |    3 | 0.0124 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 680.6 ns |  2.72 ns | 1.80 ns |  2.81 |    0.01 |    2 |      - |         - |          NA |
|                    |            |                |             |          |          |         |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           | 242.3 ns |  1.17 ns | 0.06 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           | 665.4 ns | 15.44 ns | 0.85 ns |  2.75 |    0.00 |    2 | 0.0019 |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           | 680.5 ns | 12.02 ns | 0.66 ns |  2.81 |    0.00 |    2 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 854.6 ns | 95.02 ns | 5.21 ns |  3.53 |    0.02 |    3 | 0.0124 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           | 679.2 ns | 21.49 ns | 1.18 ns |  2.80 |    0.00 |    2 |      - |         - |          NA |
