```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method             | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 3           |   241.6 ns |  0.10 ns |  0.06 ns |   241.7 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 3           |   977.5 ns |  5.19 ns |  3.43 ns |   977.3 ns |  4.05 |    0.01 |    3 |      - |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 3           |   974.8 ns |  2.56 ns |  1.69 ns |   975.2 ns |  4.03 |    0.01 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 3           | 1,191.7 ns |  5.51 ns |  3.28 ns | 1,191.4 ns |  4.93 |    0.01 |    4 | 0.0076 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 3           |   407.0 ns |  5.08 ns |  2.65 ns |   406.5 ns |  1.68 |    0.01 |    2 |      - |         - |          NA |
|                    |            |                |             |             |            |          |          |            |       |         |      |        |           |             |
| Snowflake          | MediumRun  | 15             | 2           | 10          |   241.7 ns |  0.04 ns |  0.06 ns |   241.7 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | MediumRun  | 15             | 2           | 10          |   972.0 ns |  2.09 ns |  3.00 ns |   972.5 ns |  4.02 |    0.01 |    3 |      - |      40 B |          NA |
| UuidV7             | MediumRun  | 15             | 2           | 10          |   970.8 ns |  0.63 ns |  0.88 ns |   970.8 ns |  4.02 |    0.00 |    3 |      - |         - |          NA |
| ShardPrefixed      | MediumRun  | 15             | 2           | 10          | 1,217.4 ns | 18.42 ns | 27.57 ns | 1,217.7 ns |  5.04 |    0.11 |    4 | 0.0076 |     216 B |          NA |
| DotNet_GuidNewGuid | MediumRun  | 15             | 2           | 10          |   407.3 ns |  1.22 ns |  1.71 ns |   408.4 ns |  1.69 |    0.01 |    2 |      - |         - |          NA |
