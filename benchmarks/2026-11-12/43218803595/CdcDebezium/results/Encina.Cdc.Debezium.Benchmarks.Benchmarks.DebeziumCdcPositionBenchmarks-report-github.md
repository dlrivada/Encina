```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  4.704 ns |  1.353 ns | 0.0742 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 26.791 ns | 10.093 ns | 0.5532 ns |  5.70 |    0.13 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 19.889 ns |  3.771 ns | 0.2067 ns |  4.23 |    0.07 | 0.0091 |     152 B |        6.33 |
