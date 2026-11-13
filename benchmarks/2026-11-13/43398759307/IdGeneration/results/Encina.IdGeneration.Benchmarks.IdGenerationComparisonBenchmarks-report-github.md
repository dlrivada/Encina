```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.02GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 242.1 ns |  0.13 ns | 0.08 ns |  1.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 430.6 ns |  1.89 ns | 1.25 ns |  1.78 |    3 | 0.0005 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 435.5 ns |  2.07 ns | 1.37 ns |  1.80 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 646.4 ns |  1.00 ns | 0.66 ns |  2.67 |    4 | 0.0019 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 327.3 ns |  0.68 ns | 0.41 ns |  1.35 |    2 |      - |         - |          NA |
|                    |            |                |             |          |          |         |       |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           | 242.1 ns |  1.55 ns | 0.08 ns |  1.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           | 428.7 ns | 12.52 ns | 0.69 ns |  1.77 |    3 | 0.0005 |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           | 432.3 ns |  7.15 ns | 0.39 ns |  1.79 |    3 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 649.0 ns | 14.15 ns | 0.78 ns |  2.68 |    4 | 0.0019 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           | 326.7 ns |  3.98 ns | 0.22 ns |  1.35 |    2 |      - |         - |          NA |
