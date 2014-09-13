// by 汤栋坚
#include <cstdio>
#include <string>
#include <fstream>
#include <cstring>
#include <algorithm>
#include <iostream>

using namespace std;

void polist(string &s)
{
	while (s.length() && s[s.length() - 1] == ' ')
		s.erase(s.length() - 1, 1);
}

string elide(string &s)
{
	if (s.length() <= 12)
		return s;
	return s.substr(0, 10) + "...";
}

string elide(string &s, int l, int r)
{
	return (l ? "…" : "") + s.substr(l, r + 1 - l) + (r < s.length() - 1 ? "…" : "");
}

string quo(string s)
{
	return "\"" + s + "\"";
}

bool singleLine = false;
bool noOutput = false;

string diff(int line, string &a, string &b, int s = 3)
{
	string ans = "标准输出";
	string usr = "用户输出";
	string sep = ":";
	
	if (noOutput)
		return usr + "IsEmpty";
	
	string re;
	if (!singleLine) {
		char line_[32];
		sprintf(line_, "Line %d : ", line);
		re = line_;
	}
	
	if (!(s & 1)) {
		return re += ans + "End " + usr +": " + quo(elide(b));
	} else if (!(s & 2)) {
		return re += ans + sep + quo(elide(a)) + " " + usr + "End";
	}
	
	if (a.length() <= 12 && b.length() <= 12) {
		return re + ans + sep + quo(a) + " " + usr + sep + quo(b);
	}
	
	if (!a.length() || !b.length()) {
		return re + ans + sep + quo(elide(a)) + " " + usr + sep + quo(elide(b));
	}
	
	int le = min(a.length(), b.length());
	int p = -1;
	
	for (int i = 0; i < le; ++i)
		if (a[i] != b[i]) {
			p = i;
			break;
		}
	
	if (p == -1)
		p = le - 1;
	
	int l = p;
	while (l && a[l - 1] != ' ' && l > p - 15)
		--l;
	int r = p;
	while (r + 1 < a.length() && a[r + 1] != ' ' && r < p + 15)
		++r;
	
	if (l + 10 < r) {
		l = max(l, p - 3);
		r = min(r, l + 8);
	}
	
	int br = p;
	while (br + 1 < b.length() && b[br + 1] != ' ' && br < p + 15)
		++br;
	if (r < br)
		br = max(l + 8, r);
	
	return re + ans + sep + quo(elide(a, l, r)) + " " + usr + sep + quo(elide(b, l, br));
}

int main(int argc, char *argv[])
{
	string Null;
	ifstream sin(argv[3]);
	string A;
	
	ifstream uin(argv[2]);
	string B;
	
	bool correct = false;
	string note="Correct";
	int line = 0;
	
	for (;;) {
		++line;
		if (getline(sin, A)) {
			if (getline(uin, B)) {
				polist(A);
				polist(B);
				if (A != B) {
					if (line == 1) {
						string tmp;
						singleLine = !getline(sin, tmp);
					}
					note = diff(line, A, B);
					break;
				}
			} else {
				if (line == 1)
					noOutput = true;
				if (A.length()) {
					note = diff(line, A, Null, 1);
					break;
				}
			}
		} else {
			if (getline(uin, B)) {
				if (B.length()) {
					note = diff(line, Null, B, 2);
					break;
				}
			} else {
				correct = true;
				break;
			}
		}
	}
	
//	cout << "1 ";
//	cout << A << endl << B << endl;
	cout << (correct ? "1 " : "0 ") << note << "\n";
	//cout << (correct ? "1 " : "0 ") << "\n";
	sin.close();
	uin.close();

	return 0;
}
