```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.47GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method             | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 3           | 242.6 ns |  0.05 ns |  0.03 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 3           | 507.4 ns | 10.13 ns |  6.70 ns |  2.09 |    0.03 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 3           | 485.7 ns | 12.59 ns |  8.33 ns |  2.00 |    0.03 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 3           | 667.5 ns | 13.09 ns |  8.66 ns |  2.75 |    0.03 |    4 | 0.0124 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 3           | 428.8 ns |  2.50 ns |  1.65 ns |  1.77 |    0.01 |    2 |      - |         - |          NA |
|                    |            |                |             |             |          |          |          |       |         |      |        |           |             |
| Snowflake          | MediumRun  | 15             | 2           | 10          | 242.7 ns |  0.03 ns |  0.04 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | MediumRun  | 15             | 2           | 10          | 504.8 ns |  6.45 ns |  9.65 ns |  2.08 |    0.04 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | MediumRun  | 15             | 2           | 10          | 493.7 ns |  7.67 ns | 11.48 ns |  2.03 |    0.05 |    3 |      - |         - |          NA |
| ShardPrefixed      | MediumRun  | 15             | 2           | 10          | 670.9 ns |  3.95 ns |  5.79 ns |  2.76 |    0.02 |    4 | 0.0124 |     216 B |          NA |
| DotNet_GuidNewGuid | MediumRun  | 15             | 2           | 10          | 425.2 ns |  1.88 ns |  2.76 ns |  1.75 |    0.01 |    2 |      - |         - |          NA |
