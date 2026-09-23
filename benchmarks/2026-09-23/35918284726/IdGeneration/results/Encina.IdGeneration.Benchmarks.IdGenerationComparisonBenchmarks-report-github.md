```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|------------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     |   242.4 ns |     0.05 ns |  0.03 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     |   890.9 ns |     3.25 ns |  1.94 ns |  3.68 |    0.01 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     |   977.5 ns |     5.29 ns |  2.77 ns |  4.03 |    0.01 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 1,109.0 ns |     4.74 ns |  2.82 ns |  4.58 |    0.01 |    3 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     |   562.6 ns |     2.39 ns |  1.42 ns |  2.32 |    0.01 |    2 |      - |         - |          NA |
|                    |            |                |             |            |             |          |       |         |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           |   242.3 ns |     0.34 ns |  0.02 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           |   924.8 ns | 1,008.99 ns | 55.31 ns |  3.82 |    0.20 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           |   943.0 ns |    24.91 ns |  1.37 ns |  3.89 |    0.00 |    3 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 1,122.6 ns |   344.43 ns | 18.88 ns |  4.63 |    0.07 |    3 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           |   556.9 ns |     4.10 ns |  0.22 ns |  2.30 |    0.00 |    2 |      - |         - |          NA |
