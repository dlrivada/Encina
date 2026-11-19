```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  8.197 ns |  0.7836 ns | 0.0430 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 60.170 ns | 22.5027 ns | 1.2334 ns |  7.34 |    0.13 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 47.007 ns | 10.1124 ns | 0.5543 ns |  5.73 |    0.06 | 0.0091 |     152 B |        6.33 |
