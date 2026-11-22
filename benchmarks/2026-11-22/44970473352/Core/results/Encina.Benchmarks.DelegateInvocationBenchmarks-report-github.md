```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                  | Mean         | Error      | StdDev     | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-----------:|-----------:|---------:|--------:|-------:|-------:|----------:|------------:|
| CompiledDelegate        |     27.26 ns |   0.113 ns |   0.161 ns |     1.26 |    0.01 | 0.0067 |      - |     112 B |        1.00 |
| DirectCall              |     21.69 ns |   0.055 ns |   0.077 ns |     1.00 |    0.00 | 0.0067 |      - |     112 B |        1.00 |
| ExpressionCompilation   | 52,589.13 ns | 228.014 ns | 334.220 ns | 2,424.24 |   17.34 | 0.3052 | 0.2441 |    5287 B |       47.21 |
| GenericTypeConstruction |    101.40 ns |   0.601 ns |   0.881 ns |     4.67 |    0.04 | 0.0105 |      - |     176 B |        1.57 |
| MethodInfoInvoke        |     67.42 ns |   1.014 ns |   1.517 ns |     3.11 |    0.07 | 0.0105 |      - |     176 B |        1.57 |
