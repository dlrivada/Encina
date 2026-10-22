```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method             | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     | 3           |   242.0 ns |  0.12 ns |  0.07 ns |   242.0 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 3           | 1,158.6 ns |  3.28 ns |  1.72 ns | 1,158.4 ns |  4.79 |    0.01 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 3           | 1,143.2 ns |  2.86 ns |  1.49 ns | 1,143.5 ns |  4.72 |    0.01 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 3           | 1,407.9 ns |  3.79 ns |  2.51 ns | 1,408.5 ns |  5.82 |    0.01 |    4 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     | 3           |   611.9 ns |  2.14 ns |  1.42 ns |   612.0 ns |  2.53 |    0.01 |    2 |      - |         - |          NA |
|                    |            |                |             |             |            |          |          |            |       |         |      |        |           |             |
| Snowflake          | MediumRun  | 15             | 2           | 10          |   242.0 ns |  0.05 ns |  0.07 ns |   242.0 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | MediumRun  | 15             | 2           | 10          | 1,152.0 ns |  1.53 ns |  2.04 ns | 1,152.0 ns |  4.76 |    0.01 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | MediumRun  | 15             | 2           | 10          | 1,155.0 ns |  2.52 ns |  3.78 ns | 1,154.5 ns |  4.77 |    0.02 |    3 |      - |         - |          NA |
| ShardPrefixed      | MediumRun  | 15             | 2           | 10          | 1,442.8 ns | 27.61 ns | 39.60 ns | 1,411.2 ns |  5.96 |    0.16 |    4 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | MediumRun  | 15             | 2           | 10          |   610.1 ns |  0.36 ns |  0.51 ns |   609.9 ns |  2.52 |    0.00 |    2 |      - |         - |          NA |
