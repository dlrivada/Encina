```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method             | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 3           |   242.3 ns |  0.06 ns |  0.04 ns |   242.4 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 3           |   920.6 ns |  9.12 ns |  6.03 ns |   918.4 ns |  3.80 |    0.02 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 3           |   977.9 ns |  2.68 ns |  1.40 ns |   977.7 ns |  4.04 |    0.01 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 3           | 1,135.3 ns |  4.71 ns |  3.12 ns | 1,134.9 ns |  4.68 |    0.01 |    3 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 3           |   566.6 ns |  4.33 ns |  2.26 ns |   566.6 ns |  2.34 |    0.01 |    2 |      - |         - |          NA |
|                    |            |                |             |             |            |          |          |            |       |         |      |        |           |             |
| Snowflake          | MediumRun  | 15             | 2           | 10          |   242.3 ns |  0.01 ns |  0.02 ns |   242.4 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | MediumRun  | 15             | 2           | 10          |   917.7 ns |  5.39 ns |  7.38 ns |   915.5 ns |  3.79 |    0.03 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | MediumRun  | 15             | 2           | 10          |   948.0 ns | 31.49 ns | 45.17 ns |   920.0 ns |  3.91 |    0.18 |    3 |      - |         - |          NA |
| ShardPrefixed      | MediumRun  | 15             | 2           | 10          | 1,164.0 ns | 21.45 ns | 30.77 ns | 1,158.9 ns |  4.80 |    0.12 |    4 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | MediumRun  | 15             | 2           | 10          |   560.8 ns |  1.79 ns |  2.44 ns |   560.3 ns |  2.31 |    0.01 |    2 |      - |         - |          NA |
