```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean       | Error    | StdDev  | Ratio | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |-----------:|---------:|--------:|------:|-----:|-------:|----------:|------------:|
| Snowflake          | Job-YFEFPZ | 10             | Default     |   242.1 ns |  0.02 ns | 0.01 ns |  1.00 |    1 |      - |         - |          NA |
| Ulid               | Job-YFEFPZ | 10             | Default     | 1,159.2 ns |  5.75 ns | 3.81 ns |  4.79 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | Job-YFEFPZ | 10             | Default     | 1,207.7 ns |  1.79 ns | 1.06 ns |  4.99 |    3 |      - |         - |          NA |
| ShardPrefixed      | Job-YFEFPZ | 10             | Default     | 1,390.3 ns |  2.47 ns | 1.63 ns |  5.74 |    3 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | Job-YFEFPZ | 10             | Default     |   611.7 ns |  0.46 ns | 0.24 ns |  2.53 |    2 |      - |         - |          NA |
|                    |            |                |             |            |          |         |       |      |        |           |             |
| Snowflake          | ShortRun   | 3              | 1           |   242.4 ns | 10.11 ns | 0.55 ns |  1.00 |    1 |      - |         - |          NA |
| Ulid               | ShortRun   | 3              | 1           | 1,154.0 ns | 46.85 ns | 2.57 ns |  4.76 |    3 | 0.0019 |      40 B |          NA |
| UuidV7             | ShortRun   | 3              | 1           | 1,224.1 ns | 18.63 ns | 1.02 ns |  5.05 |    3 |      - |         - |          NA |
| ShardPrefixed      | ShortRun   | 3              | 1           | 1,390.8 ns | 36.92 ns | 2.02 ns |  5.74 |    3 | 0.0114 |     216 B |          NA |
| DotNet_GuidNewGuid | ShortRun   | 3              | 1           |   612.0 ns |  1.69 ns | 0.09 ns |  2.52 |    2 |      - |         - |          NA |
