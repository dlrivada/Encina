```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  8.314 ns |  0.5034 ns | 0.0276 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 58.540 ns | 13.0444 ns | 0.7150 ns |  7.04 |    0.08 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 45.405 ns | 10.4143 ns | 0.5708 ns |  5.46 |    0.06 | 0.0091 |     152 B |        6.33 |
