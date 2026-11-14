```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  4.833 ns |  3.218 ns | 0.1764 ns |  1.00 |    0.05 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 30.653 ns | 16.920 ns | 0.9275 ns |  6.35 |    0.26 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 22.207 ns |  8.281 ns | 0.4539 ns |  4.60 |    0.17 | 0.0091 |     152 B |        6.33 |
