```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  6.592 ns |  2.445 ns | 0.1340 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 38.677 ns | 39.059 ns | 2.1410 ns |  5.87 |    0.30 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 30.949 ns |  3.010 ns | 0.1650 ns |  4.70 |    0.08 | 0.0091 |     152 B |        6.33 |
