```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  3.993 ns |  2.338 ns | 0.1282 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 26.733 ns | 11.219 ns | 0.6149 ns |  6.70 |    0.23 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 20.758 ns | 17.692 ns | 0.9697 ns |  5.20 |    0.26 | 0.0091 |     152 B |        6.33 |
