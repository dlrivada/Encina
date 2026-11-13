```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.546 ns |  7.971 ns | 0.4369 ns |  1.00 |    0.07 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 49.560 ns |  1.132 ns | 0.0621 ns |  6.58 |    0.33 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 40.358 ns | 10.093 ns | 0.5532 ns |  5.36 |    0.28 | 0.0091 |     152 B |        6.33 |
