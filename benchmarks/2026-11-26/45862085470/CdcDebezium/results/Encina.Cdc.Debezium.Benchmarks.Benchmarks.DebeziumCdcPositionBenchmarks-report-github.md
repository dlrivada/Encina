```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error    | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|---------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  6.400 ns | 2.464 ns | 0.1351 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 37.730 ns | 5.327 ns | 0.2920 ns |  5.90 |    0.11 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 30.854 ns | 1.156 ns | 0.0634 ns |  4.82 |    0.09 | 0.0091 |     152 B |        6.33 |
