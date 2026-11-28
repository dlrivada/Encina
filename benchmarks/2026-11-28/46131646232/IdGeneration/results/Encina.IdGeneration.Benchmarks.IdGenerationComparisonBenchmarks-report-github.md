```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.47GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 242.1 ns |  0.16 ns | 0.11 ns |  1.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 424.5 ns |  1.90 ns | 1.26 ns |  1.75 |    3 | 0.0005 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 435.2 ns |  1.18 ns | 0.70 ns |  1.80 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 646.3 ns |  2.56 ns | 1.70 ns |  2.67 |    4 | 0.0019 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 330.3 ns |  0.40 ns | 0.24 ns |  1.36 |    2 |      - |         - |          NA |
|                    |            |                |             |          |          |         |       |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           | 242.2 ns |  0.06 ns | 0.00 ns |  1.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           | 425.6 ns | 12.15 ns | 0.67 ns |  1.76 |    3 | 0.0005 |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           | 437.9 ns | 30.45 ns | 1.67 ns |  1.81 |    3 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 644.9 ns | 50.70 ns | 2.78 ns |  2.66 |    4 | 0.0019 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           | 331.2 ns |  3.63 ns | 0.20 ns |  1.37 |    2 |      - |         - |          NA |
