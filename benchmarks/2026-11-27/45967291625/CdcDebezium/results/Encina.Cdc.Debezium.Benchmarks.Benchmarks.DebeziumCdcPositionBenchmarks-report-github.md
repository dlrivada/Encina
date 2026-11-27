```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.16GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.521 ns |  1.371 ns | 0.0752 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 52.605 ns |  8.477 ns | 0.4647 ns |  6.99 |    0.08 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 41.185 ns | 10.830 ns | 0.5936 ns |  5.48 |    0.08 | 0.0091 |     152 B |        6.33 |
