```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.837 ns |  1.384 ns | 0.0759 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 56.298 ns | 17.523 ns | 0.9605 ns |  7.18 |    0.12 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 44.796 ns |  8.261 ns | 0.4528 ns |  5.72 |    0.07 | 0.0091 |     152 B |        6.33 |
