```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method             | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 3           | 242.3 ns |  0.03 ns |  0.02 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 3           | 692.9 ns | 13.39 ns |  8.85 ns |  2.86 |    0.03 |    2 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 3           | 651.9 ns |  3.31 ns |  1.97 ns |  2.69 |    0.01 |    2 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 3           | 862.4 ns |  8.89 ns |  5.88 ns |  3.56 |    0.02 |    3 | 0.0124 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 3           | 682.0 ns |  2.46 ns |  1.63 ns |  2.81 |    0.01 |    2 |      - |         - |          NA |
|                    |            |                |             |             |          |          |          |       |         |      |        |           |             |
| Snowflake          | MediumRun  | 15             | 2           | 10          | 242.3 ns |  0.02 ns |  0.03 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | MediumRun  | 15             | 2           | 10          | 669.9 ns |  3.22 ns |  4.62 ns |  2.76 |    0.02 |    2 | 0.0019 |      40 B |          NA |
| UuidV7             | MediumRun  | 15             | 2           | 10          | 658.2 ns |  4.01 ns |  5.63 ns |  2.72 |    0.02 |    2 |      - |         - |          NA |
| ShardPrefixed      | MediumRun  | 15             | 2           | 10          | 849.1 ns |  8.00 ns | 11.98 ns |  3.50 |    0.05 |    3 | 0.0124 |     216 B |          NA |
| DotNet_GuidNewGuid | MediumRun  | 15             | 2           | 10          | 683.7 ns |  2.56 ns |  3.59 ns |  2.82 |    0.01 |    2 |      - |         - |          NA |
