```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.22GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.719 ns |  0.4603 ns | 0.0252 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 53.657 ns | 10.6805 ns | 0.5854 ns |  6.95 |    0.07 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 42.360 ns |  7.5045 ns | 0.4113 ns |  5.49 |    0.05 | 0.0091 |     152 B |        6.33 |
