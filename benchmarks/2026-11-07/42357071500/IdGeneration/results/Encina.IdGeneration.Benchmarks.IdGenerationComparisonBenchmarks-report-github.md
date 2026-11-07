```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev  | Ratio | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|---------:|--------:|------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     |   242.0 ns |  0.06 ns | 0.04 ns |  1.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     |   817.9 ns |  2.55 ns | 1.69 ns |  3.38 |    4 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     |   789.3 ns |  2.05 ns | 1.36 ns |  3.26 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 1,015.2 ns |  3.83 ns | 2.00 ns |  4.19 |    5 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     |   673.5 ns |  0.59 ns | 0.39 ns |  2.78 |    2 |      - |         - |          NA |
|                    |            |                |             |            |          |         |       |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           |   242.0 ns |  0.98 ns | 0.05 ns |  1.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           |   784.7 ns | 42.22 ns | 2.31 ns |  3.24 |    2 | 0.0019 |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           |   756.3 ns | 22.43 ns | 1.23 ns |  3.13 |    2 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 1,012.2 ns | 26.11 ns | 1.43 ns |  4.18 |    3 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           |   664.5 ns |  4.01 ns | 0.22 ns |  2.75 |    2 |      - |         - |          NA |
