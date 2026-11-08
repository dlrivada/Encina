```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  5.109 ns | 0.2286 ns | 0.3351 ns |  1.00 |    0.09 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 29.249 ns | 0.9885 ns | 1.4177 ns |  5.75 |    0.46 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 20.860 ns | 0.3099 ns | 0.4444 ns |  4.10 |    0.28 | 0.0091 |     152 B |        6.33 |
